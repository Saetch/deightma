use std::{fmt::format, mem::MaybeUninit};

use actix_web::Error;
use ndarray::Array2;


pub fn get_by_relative_index(arr: &[[f64;4]; 4], index_x: i32, index_y: i32 ) -> f64 {
    if index_x < -1 || index_x > 2 || index_y < -1 || index_y > 2 {
        panic!("Index out of bounds");
    }
    return arr[(index_x +1) as usize][(index_y +1) as usize];
}

pub fn get_by_relative_index_int(arr: &[[i32;4]; 4], index_x: i32, index_y: i32 ) -> i32 {
    if index_x < -1 || index_x > 2 || index_y < -1 || index_y > 2 {
        panic!("Index out of bounds");
    }
    return arr[(index_x +1) as usize][(index_y +1) as usize];
}

pub fn parse_input_string_to_array(input: &str) -> Result<[[f64;4]; 4], String> {
    //check if the input is a 4x4 matrix
    {
        let rows = input.split(';').collect::<Vec<&str>>();
        if rows.len() != 4 {
            return Err("Input must have 4 rows".to_string());
        }
        for row in rows {
            let cols = row.split(',').collect::<Vec<&str>>();
            if cols.len() != 4 {
                return Err("Each row must have 4 columns".to_string());
            }
        }
    }

    let mut retur_arr = [[0.0; 4]; 4];
    let mut i = 0;
    let mut j = 0;
    let mut current_number = String::new();
    let mut result ;
    for c in input.chars() {
        if c == ',' {
            result = current_number.parse::<f64>();
            if result.is_err() {
                return Err(format!("Error parsing number: {}", result.err().unwrap()));
            }
            retur_arr[i][j] =  result.unwrap();
            current_number = String::new();
            j += 1;
        } else if c == ';' {
            result = current_number.parse::<f64>();
            if result.is_err() {
                return Err(format!("Error parsing number: {}", result.err().unwrap()));
            }
            retur_arr[i][j] =  result.unwrap();
            current_number = String::new();
            j = 0;
            i += 1;
        } else {
            current_number.push(c);
        }
    }
    if current_number.len() > 0 {
        result = current_number.parse::<f64>();
        if result.is_err() {
            return Err(format!("Error parsing number: {} when trying to parse: \"{}\"", result.err().unwrap(), current_number));
        }else {
            retur_arr[i][j] =  result.unwrap();
        
        }
    }
    return Ok(retur_arr);
}

#[allow(unused)]
//my own implementation of matrix multiplication for a 4x4 matrix. But tests have shown this approach to be about 20% slower than Array2D
pub fn array_multiplication_basic(arr1: &[[f64;4]; 4], arr2: &[[f64;4]; 4]) -> [[f64;4]; 4] {
    let mut result = [[0.0; 4]; 4];
    for i in 0..4 {
        for j in 0..4 {
            for k in 0..4 {
                result[i][j] += arr1[i][k] * arr2[k][j];
            }
        }
    }
    return result;
}

pub fn bicubic_interpolation(arr_map: &[[f64;4]; 4], x: f64, y: f64) -> Result<f64, String>{
    if x < 0.0 || x > 1.0 || y < 0.0 || y > 1.0 {
        return Err("x and y must be between 0 and 1".to_string());
    }
    println!("Arr_map: {:?}", arr_map);
    const _B : [[f64;4]; 4] = [
        [-1.,  1., -1., 1.],
        [ 0.,  0.,  0., 1.],
        [ 1.,  1.,  1., 1.],
        [ 8.,  4.,  2., 1.]
    ];

    const B_A : [[f64;4]; 4] = [
        [-1./6.,  1./2.,  -1./2.,  1./6.],
        [ 1./2.,  -1.0,  1./2.,  0.],
        [-1./3.,  -1./2.,  1.0,  -1./6.],
        [ 0.,  1.,  0.,  0.]
    ];

    const B_A_T : [[f64;4]; 4] = [
        [-1./6.,  1./2.,  -1./3.,  0.],
        [ 1./2.,  -1.0,  -1./2.,  1.],
        [-1./2.,  1./2.,  1.0,  0.],
        [ 1./6.,  0.,  -1./6.,  0.]
    ];
    let matrix_map = Array2::from_shape_fn((4, 4), |(i, j)| arr_map[i][j] as f64);


    let b_a = Array2::from_shape_fn((4, 4), |(i, j)| B_A[i][j] as f64);

    let b_a_t = Array2::from_shape_fn((4, 4), |(i, j)| B_A_T[i][j] as f64);


    let mut xb_a = Array2::zeros((1,4));
    let mut yb_a_t = Array2::zeros((4,1));
    println!("X: {} Y: {}", x, y);
    let x_matrix = Array2::from_shape_fn((1, 4), |(_i, j)| x.powi(3 - j as i32) as f64);
    println!("X matrix: {}", x_matrix);


    let y_matrix = Array2::from_shape_fn((4, 1), |(i, _j)| y.powi(3 - i as i32) as f64);
    println!("Y matrix: {}", y_matrix);
    ndarray::linalg::general_mat_mul(1., &x_matrix, &b_a, 1., &mut xb_a);

    println!("Matrix x*b_inverse: \n{}", xb_a);

    ndarray::linalg::general_mat_mul(1., &b_a_t, &y_matrix, 1., &mut yb_a_t);

    println!("Matrix y*b_invers_Translated: \n{}", yb_a_t);


    let mut first_result_matrix = Array2::zeros((1, 4));
    ndarray::linalg::general_mat_mul(1., &xb_a, &matrix_map, 1., &mut first_result_matrix);

    println!("x*b_inverse*F: \n{}", first_result_matrix);

    let mut final_result_matrix = Array2::zeros((1, 1));
    ndarray::linalg::general_mat_mul(1., &first_result_matrix, &yb_a_t, 1., &mut final_result_matrix);
    println!("Result: {}\n\n\n\n\n", final_result_matrix[[0,0]] as f64);

    return Ok(final_result_matrix[[0,0]] as f64);
}


